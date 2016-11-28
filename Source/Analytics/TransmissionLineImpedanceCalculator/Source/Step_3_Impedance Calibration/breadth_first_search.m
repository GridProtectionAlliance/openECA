function [visited,temp_component]=...
    breadth_first_search(connection_matrix,parent_node_set,temp_component,visited,...
    Bus_number_set_500KV,line_bus_info_500KV, accurate_bus, original_accurate_line_number)

parent_nodes_num = size(parent_node_set,1);
nodes_num=size(connection_matrix,2);
next_parent_set=[0,0];
for i=1:parent_nodes_num
    parent_in_component_flag = 0;  
    current_bus_idx = find(Bus_number_set_500KV==parent_node_set(i,2));
    for j=1:nodes_num
        if (connection_matrix(current_bus_idx,j)==1)||(connection_matrix(j,current_bus_idx)==1)
            current_line_number = find_line_from_buses(parent_node_set(i,2),Bus_number_set_500KV(j,:),line_bus_info_500KV);
            
            if (parent_node_set(i,2) == accurate_bus) && (current_line_number ~= original_accurate_line_number)
                continue;
            end
            
            visited_set_index = find(visited(:,1)==current_line_number);
            if visited(visited_set_index,2)==1
                continue;
            else
                if parent_in_component_flag == 0
                    temp_component = [temp_component; parent_node_set(i,:),0];
                    parent_in_component_flag = 1;
                end
                visited(visited_set_index,2)=1;
                temp_component=[temp_component;parent_node_set(i,2), Bus_number_set_500KV(j,:), current_line_number];
                if next_parent_set(1,1)==0
                    next_parent_set = [parent_node_set(i,2), Bus_number_set_500KV(j,:)];
                else
                    next_parent_set = [next_parent_set ; parent_node_set(i,2), Bus_number_set_500KV(j,:)];
                end
            end
        end
    end
end

if next_parent_set(1,1)~=0    
    [visited,temp_component]=...
        breadth_first_search(connection_matrix,next_parent_set,temp_component,visited,...
        Bus_number_set_500KV,line_bus_info_500KV, accurate_bus, original_accurate_line_number);
else
    return;
end